```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncryptReadAuditEntry           |  8,808.3 ns |   244.9 ns |  13.42 ns | 15.28 |    0.21 |    2 | 0.0610 |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 22,276.9 ns | 5,407.0 ns | 296.38 ns | 38.64 |    0.68 |    3 | 0.4883 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    576.6 ns |   162.8 ns |   8.92 ns |  1.00 |    0.02 |    1 | 0.0095 |     800 B |        1.00 |
