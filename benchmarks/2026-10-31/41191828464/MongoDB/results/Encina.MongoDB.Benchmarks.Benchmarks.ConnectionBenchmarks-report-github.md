```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method      | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| PingCommand | 102.0 μs | 10.28 μs | 0.56 μs |  1.00 | 0.6104 |  11.73 KB |        1.00 |
