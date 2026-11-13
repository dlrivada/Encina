```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.16 ns |     0.855 ns |   0.047 ns |     1.27 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.28 ns |     6.115 ns |   0.335 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,175.64 ns | 3,445.469 ns | 188.858 ns | 3,079.55 |   31.01 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    134.90 ns |    24.477 ns |   1.342 ns |     4.61 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.16 ns |    10.973 ns |   0.601 ns |     2.91 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
