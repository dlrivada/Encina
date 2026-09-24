```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 11,635.6 ns | 16,233.3 ns | 889.80 ns | 20.51 |    1.40 |    2 | 0.0610 |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 27,096.0 ns | 16,631.2 ns | 911.61 ns | 47.77 |    1.60 |    3 | 0.4883 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    567.3 ns |    195.7 ns |  10.73 ns |  1.00 |    0.02 |    1 | 0.0095 |     800 B |        1.00 |
