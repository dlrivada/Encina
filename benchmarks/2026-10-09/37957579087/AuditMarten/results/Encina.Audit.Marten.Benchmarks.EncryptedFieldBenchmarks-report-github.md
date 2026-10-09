```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,433.171 ns |    811.0821 ns |  44.4581 ns |  1.209 |    0.01 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 116,581.835 ns | 11,655.4609 ns | 638.8751 ns | 25.937 |    0.13 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,534.486 ns |    435.5462 ns |  23.8738 ns |  1.009 |    0.00 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,101.882 ns |    324.1450 ns |  17.7675 ns |  1.803 |    0.00 |    3 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.275 ns |      0.6440 ns |   0.0353 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,044.111 ns |    285.1460 ns |  15.6298 ns |  0.900 |    0.00 |    2 |        - |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  94,020.191 ns |  5,270.7874 ns | 288.9096 ns | 20.918 |    0.06 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,151.058 ns |    551.4268 ns |  30.2256 ns |  2.258 |    0.01 |    4 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,494.801 ns |    101.0193 ns |   5.5372 ns |  1.000 |    0.00 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
