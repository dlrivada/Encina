```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     34.95 ns |     1.139 ns |   0.062 ns |     1.26 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.81 ns |     1.252 ns |   0.069 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 66,758.50 ns | 7,237.238 ns | 396.697 ns | 2,400.17 |   13.38 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    131.29 ns |     5.520 ns |   0.303 ns |     4.72 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.58 ns |    12.366 ns |   0.678 ns |     3.04 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
