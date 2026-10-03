```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.22 ns |     1.082 ns |   0.059 ns |     1.25 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.17 ns |     7.548 ns |   0.414 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 63,920.47 ns | 4,841.416 ns | 265.374 ns | 2,269.25 |   29.79 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    131.33 ns |    11.968 ns |   0.656 ns |     4.66 |    0.06 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.66 ns |     1.565 ns |   0.086 ns |     3.01 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
