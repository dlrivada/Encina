```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,607.260 ns |    254.8057 ns |    13.9668 ns |  1.141 |    0.00 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 147,865.959 ns | 32,028.3858 ns | 1,755.5839 ns | 30.094 |    0.31 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,688.304 ns |    121.9161 ns |     6.6826 ns |  0.954 |    0.00 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,268.584 ns |    250.7345 ns |    13.7436 ns |  1.683 |    0.00 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.056 ns |      0.0387 ns |     0.0021 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,413.420 ns |  1,268.9399 ns |    69.5549 ns |  0.898 |    0.01 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 124,834.017 ns |  3,688.9151 ns |   202.2019 ns | 25.407 |    0.05 |    4 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,850.616 ns |    570.9390 ns |    31.2951 ns |  2.005 |    0.01 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,913.447 ns |    137.6478 ns |     7.5449 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
