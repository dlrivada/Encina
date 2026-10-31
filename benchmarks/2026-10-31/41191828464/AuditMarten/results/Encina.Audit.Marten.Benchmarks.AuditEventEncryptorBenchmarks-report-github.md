```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.40GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EncryptReadAuditEntry           |  9,525.7 ns | 2,104.11 ns | 115.33 ns | 33.19 |    0.37 |    2 | 0.3204 |      - |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 19,711.7 ns | 1,401.34 ns |  76.81 ns | 68.68 |    0.33 |    3 | 2.4719 | 0.0916 |   41713 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    287.0 ns |    21.04 ns |   1.15 ns |  1.00 |    0.00 |    1 | 0.0477 |      - |     800 B |        1.00 |
