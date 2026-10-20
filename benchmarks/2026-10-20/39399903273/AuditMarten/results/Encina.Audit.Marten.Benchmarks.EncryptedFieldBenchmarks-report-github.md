```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  3,308.5259 ns |  2,863.7929 ns |   156.9742 ns |  1.203 |    0.05 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 80,259.0837 ns | 33,733.1044 ns | 1,849.0253 ns | 29.183 |    0.70 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,583.6856 ns |    241.5473 ns |    13.2400 ns |  0.939 |    0.01 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |  4,470.6363 ns |    776.4753 ns |    42.5612 ns |  1.626 |    0.03 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |      0.3363 ns |      2.4886 ns |     0.1364 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,446.9105 ns |    981.1289 ns |    53.7790 ns |  0.890 |    0.02 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 72,196.0623 ns |  5,635.1012 ns |   308.8789 ns | 26.251 |    0.36 |    4 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  5,179.4076 ns |  1,111.0866 ns |    60.9024 ns |  1.883 |    0.03 |    3 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,750.6468 ns |    774.0515 ns |    42.4284 ns |  1.000 |    0.02 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
