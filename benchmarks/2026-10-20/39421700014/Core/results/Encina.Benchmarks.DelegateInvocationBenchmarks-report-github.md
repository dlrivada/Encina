```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     26.92 ns |     0.706 ns |   0.039 ns |     1.24 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.70 ns |     4.979 ns |   0.273 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 50,751.92 ns | 4,032.143 ns | 221.015 ns | 2,339.14 |   26.84 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    102.69 ns |     3.319 ns |   0.182 ns |     4.73 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     66.07 ns |    11.766 ns |   0.645 ns |     3.05 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
