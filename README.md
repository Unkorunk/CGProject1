# Multi-channel signal visualization and analysis system

## Description
Программа предоставляет следующие режимы:

1. About signal
    * Number of channels
    * Number of samples
    * Sample rate
    * Start and end datetime of recording
    * Duration
1. Visualization
    * Oscillogram
1. Modeling
    * Discrete
        * задержанный импульс
        * задержанный скачок
        * дискретная убывающая экспонента
        * дискретная синусоида
        * меандр
        * пила
    * Continuous
        * с экспоненциальной огибающей
        * с балансной огибающей
        * с тональной огибающей
        * ЛЧМ - сигнал
    * Random
        * Uniform white noise
        * Normal white noise
        * ARMA
    * суперпозиции
        * линейная
        * мультипликативная
1. Statistics
    * Average
    * Variance
    * Standard deviation
    * Dispersion, skewness, kurtosis
    * Minimum, maximum
    * Quantile 0.05, 0.5, 0.95
    * Histogram with a distribution
1. Analysis
    * Power spectral density
    * Amplitude spectral density
1. Spectrogram

В режиме "моделирование" доступны: предпросмотр моделируемого сигнала,
сохранение набора значений.

В режиме "статистика" доступно изменение кол-ва интервалов для построения
гистограммы распределения значений.

В режиме "анализ" доступны: обычный и логарифмический режимы, настройка
режима разрешения ситуации с нулевым отсчётом (ничего не делать,
обнулить, сделать равным модулю соседнего отсчёта), настройка полуширины
окна сглаживания, выбор интервала частот.

В режиме "спектрограмма" доступны настройки параметров яркости, высоты,
нахлёста и выбор цветовой палитры (GRAY, HOT, ICE, BlueRed).

Режимы "статистика", "анализ" и "спектрограмма" обрабатывают интервал,
выбранный на осциллограмме, если такой не выбран, то обрабатывается
весь канал целиком.

Каждый из режимов позволяет одновременно просматривать несколько каналов.

## Supported formats
1. TXT

    1. number of channels = N
    2. number of samples = M
    3. sample rate (Hz)
    4. start date in the format dd-MM-yyyy
    5. start time in the format HH:mm:ss.fff
    6. channel names separated by ';'
    7. matrix with N rows and M columns where each row separated by
       newline and each column separated by whitespace

    Example:
    ```
    3
    4
    1
    27-06-2020
    16:40:00.000
    temperature;pressure;humidity
    23 1904.1 0.95
    24 1904.0 0.96
    25 1903.1 0.97
    22 1905.1 0.98
    ```

1. WAVE (PCM)

1. MP3
