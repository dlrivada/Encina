```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| ExistsAsync_False  |  52.53 ns | 0.510 ns | 0.028 ns |  0.29 |      - |         - |        0.00 |
| ExistsAsync_True   |  60.22 ns | 3.165 ns | 0.173 ns |  0.33 |      - |         - |        0.00 |
| GetAsync_CacheHit  | 181.48 ns | 2.514 ns | 0.138 ns |  1.00 | 0.0086 |     144 B |        1.00 |
| GetAsync_CacheMiss | 127.64 ns | 9.628 ns | 0.528 ns |  0.70 |      - |         - |        0.00 |
