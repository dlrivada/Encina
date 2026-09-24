```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|--------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,918.2585 ns |   287.6698 ns |  15.7682 ns |  1.203 |    0.02 |    3 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 148,241.4880 ns | 7,892.6744 ns | 432.6241 ns | 30.141 |    0.39 |    5 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,626.5277 ns |   125.3746 ns |   6.8722 ns |  0.941 |    0.01 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,280.0959 ns |   820.2557 ns |  44.9610 ns |  1.684 |    0.02 |    4 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       0.7089 ns |     0.0456 ns |   0.0025 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,138.9324 ns |   654.9521 ns |  35.9001 ns |  0.842 |    0.01 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 129,062.9314 ns | 5,622.4209 ns | 308.1839 ns | 26.242 |    0.34 |    5 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,331.4036 ns |   347.2815 ns |  19.0357 ns |  1.897 |    0.02 |    4 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,918.9437 ns | 1,318.1187 ns |  72.2505 ns |  1.000 |    0.02 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
