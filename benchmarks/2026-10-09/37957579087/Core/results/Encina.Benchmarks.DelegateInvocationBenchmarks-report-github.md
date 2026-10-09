```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error      | StdDev    | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     38.53 ns |  11.068 ns |  0.607 ns |     1.26 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.61 ns |   1.675 ns |  0.092 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,370.90 ns | 634.816 ns | 34.796 ns | 3,017.95 |    7.91 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    139.93 ns |   5.029 ns |  0.276 ns |     4.57 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.77 ns |  13.392 ns |  0.734 ns |     2.67 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
