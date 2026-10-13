```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   6,308.643 ns |     84.191 ns |     4.6148 ns |  1.222 |    0.00 |    3 |   0.2975 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 118,256.737 ns | 23,622.089 ns | 1,294.8064 ns | 22.905 |    0.22 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   5,123.976 ns |    247.869 ns |    13.5865 ns |  0.992 |    0.00 |    2 |   0.0458 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   9,136.802 ns |    332.731 ns |    18.2381 ns |  1.770 |    0.00 |    4 |   0.6561 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.431 ns |      1.038 ns |     0.0569 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,684.955 ns |    384.630 ns |    21.0829 ns |  0.907 |    0.00 |    2 |   0.0076 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  98,629.295 ns | 19,167.323 ns | 1,050.6256 ns | 19.104 |    0.18 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,826.888 ns |  2,234.066 ns |   122.4567 ns |  2.097 |    0.02 |    4 |   1.4648 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   5,162.877 ns |    144.668 ns |     7.9297 ns |  1.000 |    0.00 |    2 |   0.0534 |        - |        - |    1496 B |        1.00 |
