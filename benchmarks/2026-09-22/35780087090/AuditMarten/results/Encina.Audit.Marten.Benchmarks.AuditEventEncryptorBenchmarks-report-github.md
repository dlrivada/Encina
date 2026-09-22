```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Mean        | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |------------:|-----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| EncryptReadAuditEntry           | 11,119.4 ns |   523.4 ns | 28.69 ns | 21.14 |    0.55 |    2 | 0.0610 |    5600 B |        7.00 |
| EncryptAuditEntry_Full_AllPii   | 25,367.0 ns | 1,452.7 ns | 79.63 ns | 48.22 |    1.25 |    3 | 0.4883 |   41712 B |       52.14 |
| EncryptAuditEntry_Minimal_NoPii |    526.3 ns |   290.4 ns | 15.92 ns |  1.00 |    0.04 |    1 | 0.0095 |     800 B |        1.00 |
