```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,224.9299 ns |    537.4728 ns |    29.4607 ns |  1.113 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 109,680.3313 ns | 22,874.7117 ns | 1,253.8401 ns | 28.883 |    0.29 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,494.9602 ns |    170.9335 ns |     9.3694 ns |  0.920 |    0.00 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   5,848.8221 ns |  1,247.3074 ns |    68.3691 ns |  1.540 |    0.02 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       0.7359 ns |      2.6959 ns |     0.1478 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,157.3109 ns |     73.1233 ns |     4.0081 ns |  0.831 |    0.00 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  92,726.5921 ns | 11,420.4089 ns |   625.9912 ns | 24.418 |    0.14 |    4 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   6,866.7338 ns |  1,049.0323 ns |    57.5010 ns |  1.808 |    0.01 |    3 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,797.4360 ns |     34.6353 ns |     1.8985 ns |  1.000 |    0.00 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
