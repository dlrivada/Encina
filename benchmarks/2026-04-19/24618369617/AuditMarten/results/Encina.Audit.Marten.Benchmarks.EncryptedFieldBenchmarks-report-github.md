```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.46GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error       | StdDev        | Median         | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|------------:|--------------:|---------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,791.162 ns |  15.5762 ns |    23.3138 ns |   5,792.521 ns |  1.186 |    0.01 |    4 |   0.2975 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 117,781.985 ns | 943.0679 ns | 1,411.5397 ns | 117,456.426 ns | 24.116 |    0.29 |    8 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,771.134 ns |  12.2906 ns |    17.6268 ns |   4,772.861 ns |  0.977 |    0.00 |    3 |   0.0458 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,257.384 ns |  41.6068 ns |    62.2751 ns |   8,253.670 ns |  1.691 |    0.01 |    5 |   0.6561 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.462 ns |   0.0084 ns |     0.0120 ns |       1.461 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,266.663 ns |  29.5135 ns |    41.3738 ns |   4,293.917 ns |  0.874 |    0.01 |    2 |   0.0076 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  96,964.827 ns | 837.3418 ns | 1,253.2938 ns |  96,626.238 ns | 19.854 |    0.26 |    7 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,958.217 ns | 166.4464 ns |   243.9750 ns |   9,916.596 ns |  2.039 |    0.05 |    6 |   1.4648 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,883.964 ns |   8.5606 ns |    12.5480 ns |   4,881.851 ns |  1.000 |    0.00 |    3 |   0.0534 |        - |        - |    1496 B |        1.00 |
