```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,826.461 ns |    841.3128 ns |    46.1152 ns |  1.191 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 151,529.800 ns |  8,582.8502 ns |   470.4550 ns | 30.962 |    0.29 |    5 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,758.888 ns |    453.7201 ns |    24.8699 ns |  0.972 |    0.01 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,634.980 ns |    379.3854 ns |    20.7954 ns |  1.764 |    0.02 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.225 ns |      0.0663 ns |     0.0036 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,193.827 ns |    347.1159 ns |    19.0266 ns |  0.857 |    0.01 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 134,545.314 ns | 23,310.7733 ns | 1,277.7421 ns | 27.492 |    0.33 |    5 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,807.964 ns |  2,399.8682 ns |   131.5449 ns |  2.208 |    0.03 |    4 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,894.407 ns |    926.7070 ns |    50.7959 ns |  1.000 |    0.01 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
