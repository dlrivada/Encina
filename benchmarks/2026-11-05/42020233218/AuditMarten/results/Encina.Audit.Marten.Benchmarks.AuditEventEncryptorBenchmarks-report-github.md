```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 11,671.1 ns | 4,456.64 ns | 244.28 ns | 19.11 |    0.37 |    2 | 0.0610 |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 25,807.9 ns | 6,376.45 ns | 349.51 ns | 42.26 |    0.57 |    3 | 0.4883 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    610.7 ns |    84.90 ns |   4.65 ns |  1.00 |    0.01 |    1 | 0.0095 |     800 B |        1.00 |
