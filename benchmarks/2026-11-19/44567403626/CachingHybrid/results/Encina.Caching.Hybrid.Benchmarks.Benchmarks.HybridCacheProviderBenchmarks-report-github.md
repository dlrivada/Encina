```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.14GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |   825.7 ns |  4.20 ns | 0.23 ns |  0.79 | 0.0067 |     616 B |        1.24 |
| ExistsAsync_True   |   867.8 ns | 40.71 ns | 2.23 ns |  0.83 | 0.0067 |     616 B |        1.24 |
| GetAsync_CacheHit  | 1,042.7 ns | 28.56 ns | 1.57 ns |  1.00 | 0.0057 |     496 B |        1.00 |
| GetAsync_CacheMiss |   785.8 ns | 16.63 ns | 0.91 ns |  0.75 | 0.0067 |     616 B |        1.24 |
