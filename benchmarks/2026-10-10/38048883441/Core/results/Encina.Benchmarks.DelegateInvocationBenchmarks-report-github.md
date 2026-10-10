```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     19.00 ns |     2.240 ns |   0.123 ns |     1.27 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     14.94 ns |     5.493 ns |   0.301 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 16,550.43 ns | 8,584.044 ns | 470.520 ns | 1,108.24 |   33.41 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     62.87 ns |     9.727 ns |   0.533 ns |     4.21 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     44.74 ns |    17.462 ns |   0.957 ns |     3.00 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
