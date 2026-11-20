```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  54.30 ns |  5.336 ns | 0.292 ns |  0.30 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.36 ns |  2.971 ns | 0.163 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 180.25 ns | 30.226 ns | 1.657 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 125.49 ns |  2.233 ns | 0.122 ns |  0.70 |      - |         - |        0.00 |
