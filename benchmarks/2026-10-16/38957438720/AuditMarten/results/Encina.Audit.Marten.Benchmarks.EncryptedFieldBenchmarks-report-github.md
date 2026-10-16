```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean          | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |--------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  4,534.959 ns |   392.2255 ns |  21.4992 ns |  1.212 |    0.01 |    3 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 95,766.377 ns | 1,273.3554 ns |  69.7969 ns | 25.600 |    0.25 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  3,725.240 ns |   414.0323 ns |  22.6945 ns |  0.996 |    0.01 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |  7,324.476 ns | 1,718.8398 ns |  94.2154 ns |  1.958 |    0.03 |    4 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |      1.034 ns |     0.4517 ns |   0.0248 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  3,290.519 ns |   123.8681 ns |   6.7896 ns |  0.880 |    0.01 |    2 |   0.0038 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 77,670.525 ns | 4,720.7682 ns | 258.7612 ns | 20.763 |    0.21 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  7,828.907 ns | 3,943.9293 ns | 216.1801 ns |  2.093 |    0.05 |    4 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  3,741.190 ns |   762.4190 ns |  41.7908 ns |  1.000 |    0.01 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
