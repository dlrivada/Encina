```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.16 ns |     0.941 ns |   0.052 ns |     1.26 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.86 ns |     2.096 ns |   0.115 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 63,342.41 ns | 5,452.132 ns | 298.850 ns | 2,273.87 |   12.33 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    128.55 ns |     4.456 ns |   0.244 ns |     4.61 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.09 ns |     5.548 ns |   0.304 ns |     3.02 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
