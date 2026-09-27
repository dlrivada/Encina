```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                       | Mean           | Error         | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|--------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,584.808 ns |    39.2169 ns |    56.2438 ns |  1.146 |    0.01 |    4 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 144,738.062 ns |   964.4306 ns | 1,443.5145 ns | 29.687 |    0.37 |    8 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,863.473 ns |    89.6464 ns |   122.7091 ns |  0.998 |    0.03 |    3 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |   8,589.199 ns |    81.4483 ns |   116.8108 ns |  1.762 |    0.03 |    5 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |       1.227 ns |     0.0025 ns |     0.0034 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   4,204.703 ns |    16.4920 ns |    23.6523 ns |  0.862 |    0.01 |    2 |   0.0153 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 126,476.364 ns | 1,460.3915 ns | 2,185.8454 ns | 25.942 |    0.48 |    7 |  41.5039 |  41.5039 |  41.5039 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |   9,801.196 ns |   218.1153 ns |   326.4647 ns |  2.010 |    0.07 |    6 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,875.687 ns |    26.7571 ns |    37.5097 ns |  1.000 |    0.01 |    3 |   0.0839 |        - |        - |    1496 B |        1.00 |
