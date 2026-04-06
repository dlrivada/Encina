```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Short_16B            |   5,198.605 ns |     95.0519 ns |     5.2101 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
| Encrypt_Medium_256B          |   5,633.509 ns |    486.8897 ns |    26.6881 ns |  1.084 |    0.00 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_Long_4KB             |   9,861.004 ns |    294.1468 ns |    16.1232 ns |  1.897 |    0.00 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_VeryLong_64KB        | 144,402.155 ns |  2,064.8896 ns |   113.1836 ns | 27.777 |    0.03 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Short_16B            |   4,438.909 ns |    242.5299 ns |    13.2939 ns |  0.854 |    0.00 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_Medium_256B          |   4,673.732 ns |    262.0603 ns |    14.3644 ns |  0.899 |    0.00 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,260.071 ns |  2,241.0170 ns |   122.8377 ns |  1.589 |    0.02 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| Decrypt_VeryLong_64KB        | 124,160.112 ns | 23,095.6944 ns | 1,265.9529 ns | 23.883 |    0.21 |    4 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| DecryptOrPlaceholder_NullKey |       1.225 ns |      0.0320 ns |     0.0018 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
