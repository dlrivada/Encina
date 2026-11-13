```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  44.33 ns |  0.316 ns | 0.017 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  49.89 ns |  3.438 ns | 0.188 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 151.03 ns | 32.534 ns | 1.783 ns |  1.00 | 0.0017 |     144 B |        1.00 |
| GetAsync_CacheMiss |  96.73 ns |  1.704 ns | 0.093 ns |  0.64 |      - |         - |        0.00 |
