```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.38GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,317.936 ns |    996.1804 ns |    54.6040 ns |  1.088 |    0.01 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 102,199.329 ns | 24,830.5900 ns | 1,361.0484 ns | 25.753 |    0.31 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,936.190 ns |    616.4857 ns |    33.7917 ns |  0.992 |    0.01 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   7,103.409 ns |  2,890.1122 ns |   158.4168 ns |  1.790 |    0.03 |    3 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.109 ns |      0.5805 ns |     0.0318 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,841.619 ns |    422.6514 ns |    23.1669 ns |  0.968 |    0.01 |    2 |   0.0038 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  82,944.248 ns |  5,773.2647 ns |   316.4521 ns | 20.901 |    0.09 |    4 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   7,039.827 ns |    770.7741 ns |    42.2487 ns |  1.774 |    0.01 |    3 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,968.397 ns |    231.4132 ns |    12.6845 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
