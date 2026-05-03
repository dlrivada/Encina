```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     34.92 ns |   0.127 ns |     0.182 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.03 ns |   0.175 ns |     0.256 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 62,542.46 ns | 939.148 ns | 1,405.673 ns | 2,231.06 |   53.18 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    130.26 ns |   1.571 ns |     2.303 ns |     4.65 |    0.09 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.16 ns |   1.564 ns |     2.293 ns |     3.04 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
