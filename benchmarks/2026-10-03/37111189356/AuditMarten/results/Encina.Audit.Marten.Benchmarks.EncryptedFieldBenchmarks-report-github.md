```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   6,068.855 ns |     43.5421 ns |     2.3867 ns |  1.175 |    0.00 |    2 |   0.2975 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 118,903.889 ns | 27,556.2702 ns | 1,510.4522 ns | 23.023 |    0.25 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   5,146.213 ns |    276.3594 ns |    15.1482 ns |  0.996 |    0.00 |    2 |   0.0458 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,811.248 ns |  1,075.2555 ns |    58.9384 ns |  1.706 |    0.01 |    3 |   0.6561 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.356 ns |      0.5186 ns |     0.0284 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,682.101 ns |    163.5647 ns |     8.9655 ns |  0.907 |    0.00 |    2 |   0.0076 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  96,076.573 ns |  8,331.8198 ns |   456.6952 ns | 18.603 |    0.08 |    4 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,902.503 ns |    835.4289 ns |    45.7927 ns |  1.917 |    0.01 |    3 |   1.4648 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   5,164.613 ns |     92.7272 ns |     5.0827 ns |  1.000 |    0.00 |    2 |   0.0534 |        - |        - |    1496 B |        1.00 |
