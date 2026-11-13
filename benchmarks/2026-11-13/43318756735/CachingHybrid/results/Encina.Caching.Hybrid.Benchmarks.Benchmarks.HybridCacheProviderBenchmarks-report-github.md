```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| ExistsAsync_False  | 762.6 ns | 495.4 ns | 27.15 ns |  0.81 |    0.03 | 0.0362 |     616 B |        1.24 |
| ExistsAsync_True   | 812.5 ns | 174.0 ns |  9.54 ns |  0.86 |    0.01 | 0.0362 |     616 B |        1.24 |
| GetAsync_CacheHit  | 946.2 ns | 216.2 ns | 11.85 ns |  1.00 |    0.02 | 0.0296 |     496 B |        1.00 |
| GetAsync_CacheMiss | 743.4 ns | 173.3 ns |  9.50 ns |  0.79 |    0.01 | 0.0362 |     616 B |        1.24 |
