```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     28.25 ns |     1.246 ns |   0.068 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     22.59 ns |     3.748 ns |   0.205 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 54,152.61 ns | 7,261.385 ns | 398.021 ns | 2,397.46 |   24.31 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    100.84 ns |     3.986 ns |   0.218 ns |     4.46 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     75.35 ns |     5.826 ns |   0.319 ns |     3.34 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
