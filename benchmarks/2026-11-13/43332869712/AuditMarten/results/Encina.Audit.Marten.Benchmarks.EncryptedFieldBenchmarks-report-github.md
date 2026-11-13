```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                       | Mean            | Error          | StdDev      | Ratio  | RatioSD | Rank | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|----------------------------- |----------------:|---------------:|------------:|-------:|--------:|-----:|---------:|---------:|---------:|----------:|------------:|
| Encrypt_Medium_256B          |   5,029.2010 ns |    305.9643 ns |  16.7709 ns |  1.194 |    0.00 |    2 |   0.4501 |   0.0076 |        - |    7568 B |        5.06 |
| Encrypt_VeryLong_64KB        | 145,809.5955 ns |  6,862.9759 ns | 376.1829 ns | 34.615 |    0.12 |    4 | 137.6953 | 137.6953 | 137.6953 |  569669 B |      380.79 |
| Decrypt_Medium_256B          |   4,552.2199 ns |    713.9032 ns |  39.1314 ns |  1.081 |    0.01 |    2 |   0.1068 |        - |        - |    1792 B |        1.20 |
| Decrypt_Long_4KB             |   7,757.3483 ns |    509.3057 ns |  27.9168 ns |  1.842 |    0.01 |    3 |   1.3275 |        - |        - |   22272 B |       14.89 |
| DecryptOrPlaceholder_NullKey |       0.6191 ns |      0.4906 ns |   0.0269 ns |  0.000 |    0.00 |    1 |        - |        - |        - |         - |        0.00 |
| Decrypt_Short_16B            |   3,791.5826 ns |     66.6927 ns |   3.6557 ns |  0.900 |    0.00 |    2 |   0.0267 |        - |        - |     504 B |        0.34 |
| Decrypt_VeryLong_64KB        | 168,044.7099 ns | 12,545.3270 ns | 687.6517 ns | 39.894 |    0.18 |    4 |  68.8477 |  68.8477 |  68.8477 |  350037 B |      233.98 |
| Encrypt_Long_4KB             |   8,565.2974 ns |  1,406.0619 ns |  77.0710 ns |  2.033 |    0.02 |    3 |   2.2125 |   0.0763 |        - |   37112 B |       24.81 |
| Encrypt_Short_16B            |   4,212.3110 ns |    243.6825 ns |  13.3571 ns |  1.000 |    0.00 |    2 |   0.0839 |        - |        - |    1496 B |        1.00 |
