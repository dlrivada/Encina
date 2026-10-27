```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  36.42 ns |  9.906 ns | 0.543 ns |  0.33 |      - |         - |        0.00 |
| ExistsAsync_True   |  40.93 ns |  5.654 ns | 0.310 ns |  0.37 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 110.15 ns | 12.755 ns | 0.699 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss |  81.91 ns | 28.338 ns | 1.553 ns |  0.74 |      - |         - |        0.00 |
