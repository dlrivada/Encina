```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.62GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  49.74 ns |  0.640 ns | 0.035 ns |  0.27 |      - |         - |        0.00 |
| ExistsAsync_True   |  63.41 ns | 12.137 ns | 0.665 ns |  0.34 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 186.36 ns |  2.880 ns | 0.158 ns |  1.00 | 0.0057 |     144 B |        1.00 |
| GetAsync_CacheMiss | 131.96 ns |  2.676 ns | 0.147 ns |  0.71 |      - |         - |        0.00 |
