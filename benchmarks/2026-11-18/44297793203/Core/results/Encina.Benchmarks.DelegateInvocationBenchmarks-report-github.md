```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.47 ns |    12.494 ns |   0.685 ns |     1.34 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.64 ns |     5.048 ns |   0.277 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,585.04 ns | 7,253.976 ns | 397.615 ns | 3,197.65 |   29.24 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    129.98 ns |     0.849 ns |   0.047 ns |     4.54 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.61 ns |    12.150 ns |   0.666 ns |     2.85 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
