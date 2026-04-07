```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                          | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
|-------------------------------- |---------:|------:|------:|-----:|----------:|------------:|
| EncryptReadAuditEntry           | 84.66 ms |    NA |  1.80 |    2 |    5824 B |        7.28 |
| EncryptAuditEntry_Full_AllPii   | 90.76 ms |    NA |  1.93 |    3 |   41936 B |       52.42 |
| EncryptAuditEntry_Minimal_NoPii | 46.97 ms |    NA |  1.00 |    1 |     800 B |        1.00 |
