```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method             | Mean     | Error      | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|-----------:|---------:|------:|--------:|----------:|------------:|
| BulkInsert_100Rows | 302.0 μs | 2,315.5 μs | 126.9 μs |  1.11 |    0.54 |  10.13 KB |        1.00 |
