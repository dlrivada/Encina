```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   6,131.163 ns |  1,135.6088 ns |    62.2465 ns |  1.195 |    0.01 |    2 |   0.2975 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 120,751.098 ns | 23,489.6687 ns | 1,287.5480 ns | 23.530 |    0.23 |    5 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   5,059.318 ns |    291.3501 ns |    15.9699 ns |  0.986 |    0.00 |    2 |   0.0458 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   9,044.294 ns |    163.0066 ns |     8.9349 ns |  1.762 |    0.01 |    3 |   0.6561 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.365 ns |      0.1306 ns |     0.0072 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,586.038 ns |    299.0123 ns |    16.3899 ns |  0.894 |    0.00 |    2 |   0.0076 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 104,950.399 ns | 35,195.6741 ns | 1,929.1937 ns | 20.451 |    0.33 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  11,087.742 ns |  1,702.4441 ns |    93.3167 ns |  2.161 |    0.02 |    4 |   1.4648 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   5,131.916 ns |    309.3709 ns |    16.9577 ns |  1.000 |    0.00 |    2 |   0.0534 |        - |        - |    1496 B |        1.00 |
