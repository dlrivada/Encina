```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   824.7 ns | 56.68 ns | 3.11 ns |  0.81 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   827.1 ns | 31.39 ns | 1.72 ns |  0.81 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,023.4 ns | 55.85 ns | 3.06 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   831.1 ns |  5.96 ns | 0.33 ns |  0.81 | 0.0067 |     616 B |        1.24 |
