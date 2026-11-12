```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 11.0.100
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  54.00 ns |  1.171 ns | 0.064 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.11 ns |  0.730 ns | 0.040 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 184.03 ns |  8.268 ns | 0.453 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 125.51 ns | 12.603 ns | 0.691 ns |  0.68 |      - |         - |        0.00 |
