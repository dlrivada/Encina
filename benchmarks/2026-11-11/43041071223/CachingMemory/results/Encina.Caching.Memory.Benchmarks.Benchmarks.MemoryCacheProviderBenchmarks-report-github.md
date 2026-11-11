```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.57 ns |  0.613 ns | 0.034 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.73 ns |  0.249 ns | 0.014 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 182.75 ns |  9.985 ns | 0.547 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 126.37 ns | 17.001 ns | 0.932 ns |  0.69 |      - |         - |        0.00 |
