```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean          | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |--------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  4,311.638 ns |    266.9398 ns |    14.6319 ns |  1.146 |    0.01 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 98,480.022 ns | 20,510.4146 ns | 1,124.2450 ns | 26.168 |    0.30 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  3,828.702 ns |  1,924.3599 ns |   105.4807 ns |  1.017 |    0.03 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |  6,353.795 ns |  1,979.1436 ns |   108.4835 ns |  1.688 |    0.03 |    3 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |      1.053 ns |      0.2814 ns |     0.0154 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  3,422.214 ns |     65.7221 ns |     3.6024 ns |  0.909 |    0.01 |    2 |   0.0038 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 78,183.652 ns |  3,271.1551 ns |   179.3031 ns | 20.775 |    0.13 |    4 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  7,069.297 ns |  1,334.5653 ns |    73.1520 ns |  1.878 |    0.02 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  3,763.535 ns |    482.8485 ns |    26.4666 ns |  1.000 |    0.01 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
