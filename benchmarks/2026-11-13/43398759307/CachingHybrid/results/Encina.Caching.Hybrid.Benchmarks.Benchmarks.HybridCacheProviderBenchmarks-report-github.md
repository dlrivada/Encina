```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.49GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  | 750.8 ns | 61.79 ns | 3.39 ns |  0.76 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   | 829.6 ns | 32.96 ns | 1.81 ns |  0.84 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 989.3 ns | 48.84 ns | 2.68 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss | 755.1 ns |  3.53 ns | 0.19 ns |  0.76 | 0.0067 |     616 B |        1.24 |
