```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.79GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 13,424.3 ns | 1,318.12 ns |  72.25 ns | 19.15 |    0.10 |    2 | 0.0610 |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 29,984.7 ns | 2,360.43 ns | 129.38 ns | 42.78 |    0.19 |    3 | 0.4883 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    700.9 ns |    35.97 ns |   1.97 ns |  1.00 |    0.00 |    1 | 0.0095 |     800 B |        1.00 |
