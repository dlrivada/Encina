```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   6,037.848 ns |    230.027 ns |    12.6086 ns |  1.196 |    0.00 |    2 |   0.2975 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 116,124.856 ns | 26,047.868 ns | 1,427.7716 ns | 23.008 |    0.25 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   5,118.735 ns |    351.612 ns |    19.2730 ns |  1.014 |    0.00 |    2 |   0.0458 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,740.195 ns |    498.418 ns |    27.3200 ns |  1.732 |    0.01 |    3 |   0.6561 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.468 ns |      1.098 ns |     0.0602 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,448.368 ns |    368.176 ns |    20.1809 ns |  0.881 |    0.00 |    2 |   0.0076 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  95,405.300 ns | 13,863.732 ns |   759.9180 ns | 18.903 |    0.14 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,704.136 ns |    854.498 ns |    46.8379 ns |  2.121 |    0.01 |    4 |   1.4648 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   5,047.108 ns |    261.316 ns |    14.3236 ns |  1.000 |    0.00 |    2 |   0.0534 |        - |        - |    1496 B |        1.00 |
