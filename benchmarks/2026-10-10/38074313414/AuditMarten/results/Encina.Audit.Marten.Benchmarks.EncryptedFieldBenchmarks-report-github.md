```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean           | Error          | StdDev        | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |---------------:|---------------:|--------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |  3,224.6044 ns |  1,366.2445 ns |    74.8885 ns |  1.086 |    0.03 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 78,974.4469 ns | 15,287.5916 ns |   837.9645 ns | 26.603 |    0.56 |    4 | 137.8174 | 137.8174 | 137.8174 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |  2,655.1919 ns |    261.2932 ns |    14.3224 ns |  0.894 |    0.02 |    2 |   0.0763 |        - |        - |    1312 B |        0.88 |
| Decrypt_Long_4KB             |  4,461.8513 ns |  3,008.3688 ns |   164.8988 ns |  1.503 |    0.06 |    3 |   0.9918 |        - |        - |   16672 B |       11.14 |
| DecryptOrPlaceholder_NullKey |      0.4852 ns |      0.0839 ns |     0.0046 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |  2,431.5583 ns |    284.9877 ns |    15.6211 ns |  0.819 |    0.02 |    2 |   0.0191 |        - |        - |     344 B |        0.23 |
| Decrypt_VeryLong_64KB        | 72,582.2089 ns | 49,113.7627 ns | 2,692.0911 ns | 24.450 |    0.91 |    4 |  41.6260 |  41.6260 |  41.6260 |  262483 B |      175.46 |
| Encrypt_Long_4KB             |  4,992.3464 ns |  1,373.0492 ns |    75.2615 ns |  1.682 |    0.04 |    3 |   2.2125 |   0.0839 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |  2,969.5704 ns |  1,192.6063 ns |    65.3708 ns |  1.000 |    0.03 |    2 |   0.0877 |        - |        - |    1496 B |        1.00 |
