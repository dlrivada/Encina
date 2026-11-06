```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           |  8,846.7 ns |  1,458.4 ns |    79.94 ns | 26.84 |    1.16 |    2 | 0.3204 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 20,524.8 ns | 18,668.0 ns | 1,023.25 ns | 62.27 |    3.78 |    3 | 2.4719 | 0.0916 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    330.2 ns |    299.8 ns |    16.43 ns |  1.00 |    0.06 |    1 | 0.0477 |      - |     800 B |        1.00 |
