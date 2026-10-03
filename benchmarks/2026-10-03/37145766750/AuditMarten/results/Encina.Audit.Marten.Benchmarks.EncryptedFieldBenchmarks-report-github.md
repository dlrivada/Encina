```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,895.3449 ns |  3,551.8126 ns | 194.6868 ns |  1.274 |    0.05 |    3 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 112,574.9764 ns | 12,548.2758 ns | 687.8133 ns | 29.304 |    0.30 |    6 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,545.3698 ns |    165.6358 ns |   9.0791 ns |  0.923 |    0.01 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   6,193.7674 ns |  3,173.9752 ns | 173.9763 ns |  1.612 |    0.04 |    4 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       0.4567 ns |      2.8757 ns |   0.1576 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,149.7832 ns |    107.8865 ns |   5.9136 ns |  0.820 |    0.01 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  92,302.2134 ns | 13,972.0200 ns | 765.8536 ns | 24.027 |    0.27 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   7,311.5442 ns |  3,110.8452 ns | 170.5159 ns |  1.903 |    0.04 |    4 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,841.8400 ns |    712.4518 ns |  39.0519 ns |  1.000 |    0.01 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
