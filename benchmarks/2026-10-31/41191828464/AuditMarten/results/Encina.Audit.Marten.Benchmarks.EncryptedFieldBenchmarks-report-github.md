```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,561.7043 ns |    694.7391 ns |  38.0810 ns |  1.120 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 147,160.8830 ns | 12,403.1833 ns | 679.8603 ns | 29.628 |    0.12 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,689.4269 ns |    943.4473 ns |  51.7135 ns |  0.944 |    0.01 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   8,188.0077 ns |  1,054.1431 ns |  57.7811 ns |  1.648 |    0.01 |    3 |   1.3275 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.6101 ns |      0.1987 ns |   0.0109 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,337.8591 ns |    235.9364 ns |  12.9325 ns |  0.873 |    0.00 |    2 |   0.0229 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 173,616.3070 ns | 13,687.5606 ns | 750.2614 ns | 34.954 |    0.14 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   9,631.3725 ns |  3,710.4040 ns | 203.3798 ns |  1.939 |    0.04 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,966.9678 ns |    126.1351 ns |   6.9139 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
