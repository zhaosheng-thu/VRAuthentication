from datetime import datetime

import joblib
import numpy as np
import os, sys, time
from data_preprocess import data_augment_and_label, read_data_latter_data_json


def split_time_series(X, n):
    num_samples, num_features = X.shape
    num_segments = n

    # 创建一个空数组，用于存储分割后的时间序列
    X_split = np.zeros((num_samples, num_segments, num_features // num_segments))

    # 对每个时间序列进行分割
    for sample_index in range(num_samples):
        time_series = X[sample_index, :]

        # 根据余数分割
        for segment in range(num_segments):
            segment_indices = np.arange(segment, num_features, num_segments)
            X_split[sample_index, segment, :] = time_series[segment_indices]

    return X_split


def main():
    current_working_directory = r"G:\CoordAuth\srt_vr_auth"
    os.chdir(current_working_directory)  # cwd的绝对路径
    positive_label = ['23']  # 正样本
    model = 'head+eye'  # model
    noise_level = 0.33  # noise level
    augmentation_time = 1  # guassion_aug
    size_list = [3]  # list of size
    all_pin_list = [1]  # pin list
    json_name = 'data_usability.json'
    threshold = 0.375
    n_segments = 6

    pin_list = all_pin_list
    data_scaled, labels, binary_labels, scaled_data_augmented, binary_labels_augmented = data_augment_and_label(
        default_authentications_per_person=9, rotdir=os.path.join(os.getcwd(), "data/"), positive_label=positive_label,
        model=model, studytype_users_dates_range=read_data_latter_data_json(current_working_directory+'/src/'+json_name)[0],
        size_list=size_list, pin_list=pin_list,
        noise_level=noise_level, augment_time=augmentation_time)

    assert len(data_scaled.shape) == len(binary_labels.shape) == 1  # for the prediction in usability study
    data_scaled = data_scaled[None, :]
    binary_labels = binary_labels[None, :]
    X_split = split_time_series(data_scaled, n_segments)
    # 对每个段独立训练分类器
    classifiers = []
    scalers = []

    for segment in range(n_segments):
        classifiers.append(joblib.load(os.path.join(current_working_directory, f"weights/classifier_segment_{segment}.pkl")))
        scalers.append(joblib.load(os.path.join(current_working_directory, f"weights/scaler_segment_{segment}.pkl")))

    for segment in range(n_segments):
        X_split[:, segment, :] = scalers[segment].transform(X_split[:, segment, :])
    X_split = np.squeeze(X_split, axis=0)
    segment_proba = [classifier.predict_proba(X_split[segment, :].reshape(1, -1))[0] for segment, classifier
                     in enumerate(classifiers)]
    avg_proba = np.mean(segment_proba, axis=0)  # 对每个类的概率进行平均
    if avg_proba[1] > threshold:
        final_prediction = 1  # choose positive
    else:
        final_prediction = 0  # negative

    print("final prediction", final_prediction)
    print("true label", np.squeeze(binary_labels, axis=0)[0])


if __name__ == "__main__":
    current_datetime = datetime.now()
    filename = "output" + str(current_datetime).replace(' ', '-').replace('.', '-').replace(':', '-') + ".txt"
    with open(filename, 'w') as file:  # 将print内容保存到文件
        # 保存当前的标准输出
        original_stdout = sys.stdout
        sys.stdout = file
        start_time = time.time()
        main()
        end_time = time.time()
        run_time = end_time - start_time
        print(f"Time：{run_time}秒")
        # 恢复原来的标准输出
        sys.stdout = original_stdout
