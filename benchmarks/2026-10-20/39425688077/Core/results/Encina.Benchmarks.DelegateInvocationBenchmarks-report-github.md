```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     34.81 ns |     2.019 ns |   0.111 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     27.80 ns |     3.370 ns |   0.185 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 63,033.76 ns | 4,932.538 ns | 270.369 ns | 2,267.67 |   15.50 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    129.64 ns |    21.461 ns |   1.176 ns |     4.66 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.71 ns |    12.772 ns |   0.700 ns |     3.01 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
