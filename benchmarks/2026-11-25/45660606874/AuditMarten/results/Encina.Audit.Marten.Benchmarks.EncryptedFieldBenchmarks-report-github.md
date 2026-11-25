```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   3,717.4110 ns |   943.3032 ns |  51.7056 ns |  1.152 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 107,816.3433 ns | 8,605.9196 ns | 471.7195 ns | 33.405 |    0.20 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,217.3754 ns |   185.1914 ns |  10.1510 ns |  0.997 |    0.01 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   5,767.6553 ns | 3,195.5886 ns | 175.1610 ns |  1.787 |    0.05 |    3 |   1.3275 |   0.0076 |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.2734 ns |     0.0337 ns |   0.0018 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   2,822.6699 ns |    35.6748 ns |   1.9555 ns |  0.875 |    0.00 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 128,382.6701 ns | 7,897.8192 ns | 432.9061 ns | 39.777 |    0.22 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   6,545.0466 ns |   673.8917 ns |  36.9383 ns |  2.028 |    0.01 |    3 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,227.5977 ns |   322.3764 ns |  17.6705 ns |  1.000 |    0.01 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
