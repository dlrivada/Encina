```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method       | Mean     | Error    | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|--------:|------:|--------:|-------:|----------:|------------:|
| OpenAndClose | 204.6 μs | 108.4 μs | 5.94 μs |  1.00 |    0.04 | 0.2441 |   5.48 KB |        1.00 |
