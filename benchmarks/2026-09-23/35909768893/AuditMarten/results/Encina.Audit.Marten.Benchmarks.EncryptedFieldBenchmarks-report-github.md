```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   4,234.1431 ns |   394.1433 ns |  21.6043 ns |  1.127 |    0.01 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 105,893.6203 ns | 4,270.8341 ns | 234.0988 ns | 28.198 |    0.21 |    5 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   3,538.2157 ns |    54.5433 ns |   2.9897 ns |  0.942 |    0.01 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   5,922.1092 ns |   570.8351 ns |  31.2894 ns |  1.577 |    0.01 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       0.6457 ns |     2.7549 ns |   0.1510 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,191.3473 ns |   151.1829 ns |   8.2868 ns |  0.850 |    0.01 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        |  90,438.7896 ns | 4,723.7507 ns | 258.9247 ns | 24.082 |    0.19 |    5 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   7,161.7002 ns |   242.5482 ns |  13.2949 ns |  1.907 |    0.01 |    4 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   3,755.6033 ns |   577.5954 ns |  31.6600 ns |  1.000 |    0.01 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
