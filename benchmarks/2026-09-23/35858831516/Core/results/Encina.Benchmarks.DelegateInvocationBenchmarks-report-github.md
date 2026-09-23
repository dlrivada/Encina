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
| CompiledDelegate        |     35.21 ns |     1.938 ns |   0.106 ns |     1.26 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.05 ns |     0.317 ns |   0.017 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 63,775.46 ns | 3,917.289 ns | 214.720 ns | 2,273.65 |    6.74 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    132.43 ns |    31.374 ns |   1.720 ns |     4.72 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     84.99 ns |     3.229 ns |   0.177 ns |     3.03 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
