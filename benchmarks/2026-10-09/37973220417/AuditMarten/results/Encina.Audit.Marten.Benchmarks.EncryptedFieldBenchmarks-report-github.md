```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,318.696 ns |     20.1559 ns |     1.1048 ns |  1.184 |    0.00 |    2 |   0.0839 |        - |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 118,269.062 ns | 30,000.7964 ns | 1,644.4449 ns | 26.318 |    0.33 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,443.390 ns |    209.3380 ns |    11.4745 ns |  0.989 |    0.00 |    2 |   0.0153 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,080.683 ns |    237.6562 ns |    13.0267 ns |  1.798 |    0.01 |    3 |   0.1984 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.285 ns |      0.2630 ns |     0.0144 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,943.018 ns |    193.0484 ns |    10.5816 ns |  0.877 |    0.00 |    2 |        - |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  95,460.497 ns | 19,937.1852 ns | 1,092.8244 ns | 21.243 |    0.23 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  10,219.517 ns |    833.2087 ns |    45.6710 ns |  2.274 |    0.01 |    4 |   0.4425 |   0.0153 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,493.826 ns |    358.0827 ns |    19.6277 ns |  1.000 |    0.01 |    2 |   0.0153 |        - |        - |    1496 B |        1.00 |
