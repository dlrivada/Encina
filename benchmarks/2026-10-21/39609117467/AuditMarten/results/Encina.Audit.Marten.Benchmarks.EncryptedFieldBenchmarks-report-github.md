```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,465.7727 ns |   235.4411 ns |  12.9053 ns |  1.145 |    0.02 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 112,413.2507 ns | 5,138.2588 ns | 281.6453 ns | 28.828 |    0.44 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,524.8523 ns |    52.5713 ns |   2.8816 ns |  0.904 |    0.01 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   6,331.0144 ns | 1,257.5243 ns |  68.9292 ns |  1.624 |    0.03 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       0.8222 ns |     0.0486 ns |   0.0027 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,133.0352 ns |    75.9471 ns |   4.1629 ns |  0.803 |    0.01 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  94,178.9854 ns | 8,670.4859 ns | 475.2586 ns | 24.152 |    0.38 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   7,975.9927 ns |   622.7695 ns |  34.1361 ns |  2.045 |    0.03 |    4 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,900.1813 ns | 1,243.6775 ns |  68.1702 ns |  1.000 |    0.02 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
